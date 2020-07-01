% function to read in MCPP infile template, replace strings a1,s1,sp1
% with inverse iterate values and generate pmc and dmc results
function [F,J]=pmc_F_dmc_J(muamus,rhoMidpoints,measData)
% the following code assumes 1-layer tissue with varying mua and mus only
% g and n are fixed and not optimized
% determine rho bins from midpoints
rho=zeros(size(rhoMidpoints,2)+1,1);
rho(1) = rhoMidpoints(1) - (rhoMidpoints(2) - rhoMidpoints(1))/2;
rho(2:end) = rhoMidpoints(1:end) + rhoMidpoints(1);
% specify detector based on perturbed optical properties
mua = muamus(1);
mus=muamus(2);
musp = mus*(1-0.8);
% CKH update to read in
gfix = 0.8;
nfix = 1.4;
% replace MCPP infile with updated OPs
infile_PP='infile_PP_pMC_est.txt';
[status]=system('cp infile_PP_pMC_est_template.txt infile_PP_pMC_est.txt');
[status]=system(sprintf('./sub_ops.sh a1 %f %s',mua,infile_PP));
[status]=system(sprintf('./sub_ops.sh s1 %f %s',mus,infile_PP));
[status]=system(sprintf('./sub_ops.sh sp1 %f %s',musp,infile_PP));
% run MCPP with updated infile
[status]=system(sprintf('./mc_post infile=%s',infile_PP));
[R,pmcR,dmcRmua,dmcRmus]=load_for_inv_results('PP_pMC_est');
F=pmcR';
% option: normalize forward model by measured data
%F = F./measData;
% set jacobian derivative information
J = [ dmcRmua dmcRmus ];
end
